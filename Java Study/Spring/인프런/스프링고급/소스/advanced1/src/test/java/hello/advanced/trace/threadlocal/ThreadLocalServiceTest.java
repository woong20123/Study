package hello.advanced.trace.threadlocal;

import hello.advanced.trace.threadlocal.code.FiledService;
import hello.advanced.trace.threadlocal.code.ThreadLocalService;
import lombok.extern.slf4j.Slf4j;
import org.junit.jupiter.api.Test;

@Slf4j
public class ThreadLocalServiceTest {

    private ThreadLocalService threadLocalService = new ThreadLocalService();

    @Test
    void field() {
        log.info("main start");
        Runnable userA = () -> {
            threadLocalService.logic("UserA");
        };
        Runnable userB = () -> {
            threadLocalService.logic("UserB");
        };

        Thread threadA = new Thread(userA);
        threadA.setName("thread-A");
        Thread threadB = new Thread(userB);
        threadB.setName("thread-B");

        threadA.start();
        sleep(100); // 동시성 문제가 발생 X
        threadB.start();
        sleep(3000);
        log.info("main exit");
    }

    private void sleep(int milli){
        try {
            Thread.sleep(milli);
        }catch (InterruptedException e) {
            e.printStackTrace();;
        }
    }
}
