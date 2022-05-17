package hello.proxy.jdkdynamic;

import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.junit.jupiter.api.Test;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;

@Slf4j
public class ReflectionTest {
    @Test
    void reflection0() {
        Hello  target = new Hello();

        // 공통 로직1 시작
        log.info("start");
        String result1 = target.callA();
        log.info("result={}", result1);
        // 공통 로직1 종료

        // 공통 로직2 시작
        log.info("start");
        String result2 = target.callB();
        log.info("result={}", result2);
        // 공통 로직2 종료
    }

    @SneakyThrows
    @Test
    void reflection1(){
        // 클래스 정보
        var classHello = Class.forName("hello.proxy.jdkdynamic.ReflectionTest$Hello");
        Hello target = new Hello();
        var methodCallA = classHello.getMethod("callA");
        Object result1 = methodCallA.invoke(target);
        log.info("reslut1={}", result1);

        var methodCallB = classHello.getMethod("callB");
        Object result2 = methodCallB.invoke(target);
        log.info("reslut2={}", result2);

    }

    @SneakyThrows
    @Test
    void reflection2(){
        // 클래스 정보
        var classHello = Class.forName("hello.proxy.jdkdynamic.ReflectionTest$Hello");
        Hello target = new Hello();
        dynamicCall(classHello.getMethod("callA"), target);

        dynamicCall(classHello.getMethod("callB"), target);

    }

    private void dynamicCall(Method m, Object target) throws InvocationTargetException, IllegalAccessException {
        log.info("start");
        Object result = m.invoke(target);
        log.info("result={}", result);
    }

    @Slf4j
    static class Hello {
        public  String callA() {
            log.info("callA");
            return "A";
        }

        public String callB() {
            log.info("callB");
            return "B";
        }
    }
}
