package com.woong.test;

public class Main {
	
	public static void main(String[] args) {
		new Thread(()->{
			System.out.println("���� ������ ���");
		}).start();
	}

}
