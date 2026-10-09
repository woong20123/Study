#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Antigravity Hook: Context Usage Display
세션 종료 시 토큰 사용량 및 컨텍스트 요약을 터미널에 출력합니다.
"""

import json
import sys
import os
import io
from datetime import datetime

# Windows 인코딩 강제 UTF-8
if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
if sys.stderr.encoding and sys.stderr.encoding.lower() != "utf-8":
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

# 모델별 컨텍스트 한도 (토큰 기준)
MODEL_CONTEXT_LIMITS = {
    "claude-sonnet-4-5": 200_000,
    "claude-sonnet-4-6": 200_000,
    "claude-3-5-sonnet": 200_000,
    "claude-3-opus":     200_000,
    "claude-3-haiku":    200_000,
    "gemini-2.5-pro":    1_048_576,
    "gemini-2.5-flash":  1_048_576,
    "gemini-2.0-flash":  1_048_576,
    "default":           200_000,
}

ANSI = {
    "reset":  "\033[0m",
    "bold":   "\033[1m",
    "cyan":   "\033[36m",
    "green":  "\033[32m",
    "yellow": "\033[33m",
    "red":    "\033[31m",
    "blue":   "\033[34m",
    "gray":   "\033[90m",
}


def get_context_limit(model_name: str) -> int:
    if not model_name:
        return MODEL_CONTEXT_LIMITS["default"]
    model_lower = model_name.lower()
    for key, limit in MODEL_CONTEXT_LIMITS.items():
        if key in model_lower:
            return limit
    return MODEL_CONTEXT_LIMITS["default"]


def make_bar(ratio: float, width: int = 28) -> str:
    filled = int(ratio * width)
    empty = width - filled
    if ratio < 0.5:
        color = ANSI["green"]
    elif ratio < 0.8:
        color = ANSI["yellow"]
    else:
        color = ANSI["red"]
    bar = color + "█" * filled + ANSI["gray"] + "░" * empty + ANSI["reset"]
    return f"[{bar}]"


def fmt_num(n: int) -> str:
    return f"{n:,}"


def main():
    try:
        payload = json.load(sys.stdin)
    except Exception:
        payload = {}

    transcript_path = payload.get("transcriptPath", "")
    model_name = payload.get("modelName", "")
    context_limit = get_context_limit(model_name)

    total_input = 0
    total_output = 0
    total_cache_read = 0
    step_count = 0
    session_start = None
    session_end = None
    current_context = 0

    if transcript_path and os.path.exists(transcript_path):
        all_lines = []
        with open(transcript_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    step = json.loads(line)
                except json.JSONDecodeError:
                    continue
                all_lines.append(step)

        for step in all_lines:
            # 세션 시간 범위
            created_at = step.get("created_at")
            if created_at:
                try:
                    dt = datetime.fromisoformat(created_at.replace("Z", "+00:00"))
                    if session_start is None or dt < session_start:
                        session_start = dt
                    if session_end is None or dt > session_end:
                        session_end = dt
                except Exception:
                    pass

            # 토큰 누적
            if step.get("source") == "MODEL" and step.get("type") == "PLANNER_RESPONSE":
                step_count += 1
                total_input += step.get("input_tokens", 0)
                total_output += step.get("output_tokens", 0)
                total_cache_read += step.get("cache_read_tokens", 0)

        # 마지막 응답의 input_tokens = 현재 컨텍스트 크기
        for step in reversed(all_lines):
            if step.get("source") == "MODEL" and step.get("type") == "PLANNER_RESPONSE":
                current_context = (
                    step.get("input_tokens", 0)
                    + step.get("cache_read_tokens", 0)
                )
                break

    ratio = min(current_context / context_limit, 1.0) if context_limit > 0 else 0
    remaining = context_limit - current_context
    pct = ratio * 100

    # 세션 시간
    duration_str = ""
    if session_start and session_end:
        delta = session_end - session_start
        minutes, seconds = divmod(int(delta.total_seconds()), 60)
        hours, minutes = divmod(minutes, 60)
        if hours > 0:
            duration_str = f"{hours}h {minutes}m {seconds}s"
        elif minutes > 0:
            duration_str = f"{minutes}m {seconds}s"
        else:
            duration_str = f"{seconds}s"

    W = ANSI
    sep = W["gray"] + "─" * 50 + W["reset"]

    def fmt_k(n: int) -> str:
        return f"{n/1000:.1f}k"

    out = [
        "",
        W["bold"] + W["cyan"] + "┌── 📊 Context Usage " + "─" * 29 + "┐" + W["reset"],
        "",
        f"  {make_bar(ratio)}  {W['cyan']}{fmt_k(current_context)}{W['reset']} / {fmt_k(context_limit)} ({pct:.1f}%)",
        "",
        sep,
        "",
        f"  {W['bold']}세션 누적{W['reset']}",
        f"  Input  : {W['cyan']}{fmt_num(total_input)}{W['reset']} tokens",
        f"  Output : {W['green']}{fmt_num(total_output)}{W['reset']} tokens",
        f"  Cache↩ : {W['gray']}{fmt_num(total_cache_read)}{W['reset']} tokens",
        f"  Turns  : {W['blue']}{step_count}{W['reset']} 회",
    ]

    if duration_str:
        out.append(f"  Time   : {W['gray']}{duration_str}{W['reset']}")
    if model_name:
        out.append(f"  Model  : {W['gray']}{model_name}{W['reset']}")

    out += [
        "",
        W["bold"] + W["cyan"] + "└" + "─" * 49 + "┘" + W["reset"],
        "",
    ]

    # stderr로 출력 (stdout은 hook JSON 반환용)
    print("\n".join(out), file=sys.stderr)
    print("{}", file=sys.stdout)


if __name__ == "__main__":
    main()
