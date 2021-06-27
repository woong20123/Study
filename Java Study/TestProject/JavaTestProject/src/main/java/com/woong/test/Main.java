package com.woong.test;

import java.text.SimpleDateFormat;
import java.util.Date;

public class Main {
	public static void main(String args[]) {
		Buyer b = new Buyer();

		b.buy(new Tv());
		b.buy(new Computer());

		System.out.println("현재 남은 돈은 " + b.money + "만원입니다.");
		System.out.println("현재 보너스 점수는 " + b.bonusPoint + "점입니다.");
	}
}

class Product {
	int price; // 제품의 가격
	int bonusPoint; // 구매시 제공되는 마일리지

	Product(int price) {
		this.price = price;
		bonusPoint = (int) (price / 10.0); // 보너스는 제품 가격의 10%
	}
}

class Tv extends Product {
	Tv() {
		super(100);
	}

	public String toString() {
		return "Tv";
	}
}

class Computer extends Product {
	Computer() {
		super(200);
	}

	public String toString() {
		return "Computer";
	}
}

class Buyer {
	int money = 1000;
	int bonusPoint = 0;

	void buy(Product p) {
		if (money < p.price) {
			System.out.println("잔액이 부족해서 물건을 살수 없습니다.");
			return;
		}

		money -= p.price;
		bonusPoint += p.bonusPoint;
		System.out.println(p + "을/를 구입하셧습니다.");
	}
}
