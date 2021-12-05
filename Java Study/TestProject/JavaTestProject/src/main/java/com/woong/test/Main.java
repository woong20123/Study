package com.woong.test;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

class Juicer {
	static List<Number> makeJuice(List<? extends Number> datas){
		return new ArrayList<Number>(datas);
	}
}

public class Main {
	public static void main(String args[]) {
		List<Integer> list = new ArrayList<>();
		Juicer.makeJuice(list);
	}
}
