package com.woong.backend.entity;

public class ExeTimeCalcalator implements Calculator {
    private Calculator delegate;

    public ExeTimeCalcalator(Calculator delegate){
        this.delegate = delegate;
    }

    @Override
    public long factorial(long num){
        long start = System.nanoTime();
        long result = delegate.factorial(num);
        long end = System.nanoTime();

        System.out.printf("%s.factorial(%d) 실행시간 = %d result = %d",
                delegate.getClass().getSimpleName(),
                num, (end - start), result);
        return result;
    }
}
