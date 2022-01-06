package com.woong.backend;

import com.woong.backend.config.AppCtx;
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

		ApplicationContext ctx = new AnnotationConfigApplicationContext(AppCtx.class);

		MemberRegisterService regSvc =
				ctx.getBean("memberRegSvc", MemberRegisterService.class);

		ChangePasswordService changeSvc =
				ctx.getBean("changePwdSvc", ChangePasswordService.class);

		changeSvc.ChangePasswordService("", "", "");

		SpringApplication.run(BackendApplication.class, args);

		List<Applicant> applicantList = List.of(new Applicant(new String("안시혁"), 8, new String("A")),
				new Applicant(new String("홍진희"), 7, new String("A")),new Applicant(new String("양민자"), 9, new String("A")), new Applicant(new String("복세연"), 4, new String("A")),
				new Applicant(new String("조우빈"), 6, new String("B")),new Applicant(new String("정서윤"), 5, new String("B")), new Applicant(new String("임해남"), 7, new String("B"))
				);

		var a = applicantList.stream().map(Applicant::getGrade).filter(g -> g >= 6).distinct().reduce(0L, Long::sum);
		var b = applicantList.stream().map(Applicant::getGroup).filter(name -> name == "B").count();

		var data = applicantList.stream()
				.filter(g -> g.getGrade() >= 7).collect(Collectors.groupingBy(Applicant::getGroup, Collectors.counting()));
		var c= data.get("A");

	}

}
