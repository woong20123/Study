package hello.advanced.trace.threadlocal.code;

import lombok.extern.slf4j.Slf4j;

@Slf4j
public class ThreadLocalService {
    private ThreadLocal<String> nameStore = new ThreadLocal<>();
    // ThreadLocal get() : 값 가져오기
    //             set() : 값 셋팅
    //             remove() : 값 제거

    public String logic(String name) {
        log.info("저장 name={} -> nameStore={}", name, nameStore.get());
        nameStore.set(name);
        sleep(1000);
        log.info("조회 nameStore={}", nameStore.get());
        return nameStore.get();
    }

    private void sleep(int milli){
        try {
            Thread.sleep(milli);
        }catch (InterruptedException e) {
            e.printStackTrace();;
        }
    }
}
