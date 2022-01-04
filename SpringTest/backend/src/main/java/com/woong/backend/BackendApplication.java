package com.woong.backend;

import com.woong.backend.config.AppCtx;
import com.woong.backend.service.MemberRegisterService;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.ApplicationContext;
import org.springframework.context.annotation.AnnotationConfigApplicationContext;

@SpringBootApplication
public class BackendApplication {

	public static void main(String[] args) {

		ApplicationContext ctx = new AnnotationConfigApplicationContext(AppCtx.class);

		MemberRegisterService regSvc =
				ctx.getBean("memberRegSvc", MemberRegisterService.class);

		SpringApplication.run(BackendApplication.class, args);
	}

}
