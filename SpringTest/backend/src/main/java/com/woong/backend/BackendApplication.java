package com.woong.backend;

import com.woong.backend.config.AppCtx;
import com.woong.backend.entity.Calculator;
import com.woong.backend.entity.ExeTimeCalcalator;
import com.woong.backend.entity.ImpeCalculator;
import com.woong.backend.entity.RecCalculator;
import com.woong.backend.service.ChangePasswordService;
import com.woong.backend.service.MemberRegisterService;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.ApplicationContext;
import org.springframework.context.annotation.AnnotationConfigApplicationContext;
import com.woong.backend.repository.Applicant;

import java.util.List;
import java.util.stream.Collectors;

@SpringBootApplication
public class BackendApplication {


	public static void main(String[] args) {
		AnnotationConfigApplicationContext ctx =
				new AnnotationConfigApplicationContext(AppCtx.class);

		Calculator cal = ctx.getBean("calculator", Calculator.class);
		long fineFact = cal.factorial(5);
		System.out.println("cal.factorial(5) = " + fineFact);
		System.out.println(cal.getClass().getName());
	}

}
