"""stock_price.py 오프라인 단위 테스트 (네트워크 불필요). 실행: python test_stock_price.py"""
import json
import os
import tempfile
import time
import unittest
from datetime import datetime, timedelta

import stock_price as sp


class SplitAdjustTest(unittest.TestCase):
    NOW = datetime(2026, 10, 8, 10, 0)

    def test_forward_split_not_yet_adjusted(self):
        # 10:1 분할 다음 날, 전일 종가가 분할 전(1200)으로 남아 있음 → 120으로 보정
        prev, applied = sp.adjust_prev_close_for_split(121.0, 1200.0, [(self.NOW - timedelta(days=1), 10.0)], self.NOW)
        self.assertAlmostEqual(prev, 120.0)
        self.assertEqual(applied[1], 10.0)

    def test_already_adjusted_is_left_alone(self):
        # yfinance가 이미 보정한 전일 종가(120)를 또 나누면 안 된다
        prev, applied = sp.adjust_prev_close_for_split(121.0, 120.0, [(self.NOW - timedelta(days=1), 10.0)], self.NOW)
        self.assertEqual(prev, 120.0)
        self.assertIsNone(applied)

    def test_reverse_split(self):
        # 1:10 역분할(ratio 0.1): 전일 2.0 → 20.0
        prev, applied = sp.adjust_prev_close_for_split(19.5, 2.0, [(self.NOW - timedelta(days=2), 0.1)], self.NOW)
        self.assertAlmostEqual(prev, 20.0)
        self.assertIsNotNone(applied)

    def test_old_split_is_ignored(self):
        # 한 달 전 분할은 전일 종가와 무관 → 진짜 급락으로 보고 그대로 둔다
        prev, applied = sp.adjust_prev_close_for_split(60.0, 100.0, [(self.NOW - timedelta(days=30), 2.0)], self.NOW)
        self.assertEqual(prev, 100.0)
        self.assertIsNone(applied)

    def test_ratio_text(self):
        self.assertEqual(sp.split_ratio_text(10.0), '10:1')
        self.assertEqual(sp.split_ratio_text(4.0), '4:1')
        self.assertEqual(sp.split_ratio_text(0.1), '1:10 (역분할)')


class CacheTest(unittest.TestCase):
    def setUp(self):
        fd, self.path = tempfile.mkstemp(suffix='.json')
        os.close(fd)
        os.remove(self.path)

    def tearDown(self):
        if os.path.exists(self.path):
            os.remove(self.path)

    def test_put_get_and_persist(self):
        c = sp.Cache(self.path, 3600)
        c.put('quote', 'AAPL', {'symbol': 'AAPL', 'price': 1.0})
        data, age = sp.Cache(self.path, 3600).get('quote', 'AAPL')  # 다른 인스턴스(=다른 실행)에서 읽힘
        self.assertEqual(data['price'], 1.0)
        self.assertLess(age, 5)

    def test_expired_entry(self):
        c = sp.Cache(self.path, 3600)
        c.put('quote', 'AAPL', {'symbol': 'AAPL', 'price': 1.0})
        raw = json.load(open(self.path, encoding='utf-8'))
        raw['quote:AAPL']['ts'] = time.time() - 3601  # 1시간 1초 전
        json.dump(raw, open(self.path, 'w', encoding='utf-8'))
        self.assertEqual(sp.Cache(self.path, 3600).get('quote', 'AAPL'), (None, None))

    def test_ttl_is_capped_at_one_hour(self):
        self.assertEqual(sp.Cache(self.path, 99999).ttl, 3600)

    def test_errors_are_not_cached(self):
        c = sp.Cache(self.path, 3600)
        c.put('quote', 'BAD', {'symbol': 'BAD', 'error': 'x'})
        self.assertEqual(c.get('quote', 'BAD'), (None, None))

    def test_disabled(self):
        c = sp.Cache(self.path, 3600, enabled=False)
        c.put('quote', 'AAPL', {'symbol': 'AAPL'})
        self.assertFalse(os.path.exists(self.path))

    def test_invalidate_symbol_only(self):
        c = sp.Cache(self.path, 3600)
        c.put('quote', 'NVDA', {'symbol': 'NVDA'})
        c.put('dividend', 'NVDA', {'symbol': 'NVDA'})
        c.put('quote', 'NVDAX', {'symbol': 'NVDAX'})
        c.invalidate('NVDA')
        self.assertEqual(c.get('dividend', 'NVDA'), (None, None))
        self.assertIsNotNone(c.get('quote', 'NVDAX')[0])

    def test_corrupt_file(self):
        open(self.path, 'w').write('{broken')
        self.assertEqual(sp.Cache(self.path, 3600).get('quote', 'AAPL'), (None, None))


class TargetPriceTest(unittest.TestCase):
    """target_price.py 계산 검증. 예시: KO 2030년 EPS 10.5 × PER 18, 목표 10%, 5년 평균 배당 2.92%."""

    def setUp(self):
        import target_price as tp
        from datetime import date
        self.tp, self.date = tp, date
        self.t = tp.build_target('KO', 2030, 10.5, 18, 10.0, 2.92, date(2026, 10, 8))

    def test_target_price_and_growth(self):
        self.assertAlmostEqual(self.t['target_price'], 189.0)
        self.assertAlmostEqual(self.t['growth_pct'], 7.08)
        self.assertEqual(self.t['target_date'], '2030-12-31')

    def test_quarters_from_input_quarter(self):
        rows = self.tp.quarter_schedule(self.date(2026, 10, 8), self.date(2030, 12, 31))
        self.assertEqual(rows[0], ('2026Q4', self.date(2026, 10, 8)))   # 첫 분기 기준일은 입력일
        self.assertEqual(rows[1], ('2027Q1', self.date(2027, 1, 1)))
        self.assertEqual(rows[-1], ('2030Q4', self.date(2030, 10, 1)))
        self.assertEqual(len(rows), 17)                                  # 2026Q4 ~ 2030Q4

    def test_buy_price_formula(self):
        # 손계산: 2026-10-08 → 2030-12-31 = 1545일 = 4.2300년, 189 / 1.0708^4.2300
        on = self.date(2026, 10, 8)
        expected = 189.0 / (1.0708 ** (1545 / 365.25))
        self.assertAlmostEqual(self.tp.buy_price(189.0, 7.08, on, self.date(2030, 12, 31)), expected, places=6)
        self.assertAlmostEqual(expected, 141.5, delta=0.5)

    def test_buy_price_rises_each_quarter_to_target(self):
        prices = [p for _, _, p in self.tp.schedule_rows(self.t)]
        self.assertTrue(all(a < b for a, b in zip(prices, prices[1:])))  # 시간이 갈수록 허용 매입가 상승
        self.assertLess(prices[-1], 189.0)

    def test_dividend_exceeds_return(self):
        t = self.tp.build_target('X', 2030, 10, 10, 2.0, 3.0, self.date(2026, 10, 8))
        self.assertLess(t['growth_pct'], 0)
        self.assertGreater(self.tp.schedule_rows(t)[0][2], 100.0)        # 매입 목표가 > 목표 주가

    def test_past_year_rejected(self):
        with self.assertRaises(ValueError):
            self.tp.build_target('KO', 2025, 10, 10, 10, 3, self.date(2026, 10, 8))


if __name__ == '__main__':
    unittest.main(verbosity=1)
