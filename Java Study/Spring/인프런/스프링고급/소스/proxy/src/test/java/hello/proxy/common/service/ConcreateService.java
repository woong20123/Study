package hello.proxy.common.service;

import lombok.extern.slf4j.Slf4j;

@Slf4j
public class ConcreateService {
    
    public void call(){
      log.info("ConcreateService 호출");
    }
}
