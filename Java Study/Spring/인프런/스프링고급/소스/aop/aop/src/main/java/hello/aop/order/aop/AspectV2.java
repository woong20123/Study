package hello.aop.order.aop;

import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.aspectj.lang.ProceedingJoinPoint;
import org.aspectj.lang.annotation.Around;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.annotation.Pointcut;

@Slf4j
@Aspect
public class AspectV2 {
    @Pointcut("execution(* hello.aop.order..*(..))")
    private void allOrder() {} // pointcut signature

    @SneakyThrows
    @Around("allOrder()")
    public Object doLog(ProceedingJoinPoint joinPoint){
        log.info("[log] {}", joinPoint.getSignature());
        return joinPoint.proceed();
    }

}
