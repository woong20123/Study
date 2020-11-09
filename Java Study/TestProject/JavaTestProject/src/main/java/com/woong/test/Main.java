package com.woong.test;

public class Main {
	
	public static void main(String[] args) {
		new Thread(()->{
			System.out.println("람다 포현식 사용");
		}).start();
	}

}
