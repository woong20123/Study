package hello.advanced.trace.threadlocal;

import hello.advanced.trace.threadlocal.code.FiledService;
import lombok.extern.slf4j.Slf4j;
import org.junit.jupiter.api.Test;

@Slf4j
public class FiledServiceTest {

    private FiledService filedService = new FiledService();

    @Test
    void field() {
        log.info("main start");
        Runnable userA = () -> {
            filedService.logic("UserA");
        };
        Runnable userB = () -> {
            filedService.logic("UserB");
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
