package com.woong.backend;


import com.woong.backend.config.AppCtx;
import com.woong.backend.spring.InterviewRoom;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.AnnotationConfigApplicationContext;

public class BackendApplication {


	public static void main(String[] args) {
		AnnotationConfigApplicationContext ctx = new AnnotationConfigApplicationContext(AppCtx.class);
		InterviewRoom room = ctx.getBean("interviewRoom", InterviewRoom.class);
		room.show();
	}

}
