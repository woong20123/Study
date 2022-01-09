package com.woong.backend.entity;

import org.aspectj.lang.ProceedingJoinPoint;
import org.aspectj.lang.Signature;
import org.aspectj.lang.annotation.Around;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.annotation.Pointcut;

import java.lang.reflect.Array;
import java.util.Arrays;

@Aspect
public class ExeTimeAspect {

    // 공통 기능을 적용할 대상을 지정합니다.
    // execution(접근제어자 반환형 패키지를포함한클래스경로 메소드파라미터
    @Pointcut("execution(public long factorial(long))")
    private void publicTarget(){
    }

    // publicTarget에 정의된 대상에 공통기능을 적용하는 것을 지정합니다.
    @Around("publicTarget()")
    public Object measure(ProceedingJoinPoint joinPoint) throws Throwable {
        // ProceedingJoinPoint는 프록시 대상 객체의 메서드를 호출시 사용
        // java에서는 메서드이름, 파라미터를 합쳐서 메서드 시그니쳐라고 부릅니다.
        // joinPoint.getSignature는 프록시 대상의 메서드 시그니쳐를 구하는데 사용합니다.
        long start = System.nanoTime();
        try{
            Object result = joinPoint.proceed();
            return result;
        }finally {
            long finish = System.nanoTime();
            Signature sig = joinPoint.getSignature();
            System.out.printf("%s.%s(%s) 실행시간 : %d ns\n",
                    joinPoint.getTarget().getClass().getSimpleName(),
                    sig.getName(), Arrays.toString(joinPoint.getArgs()),
                    (finish - start)
            );
        }
    }
}
