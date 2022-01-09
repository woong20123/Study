package com.woong.backend.config;

import com.woong.backend.entity.Calculator;
import com.woong.backend.entity.ExeTimeAspect;
import com.woong.backend.entity.RecCalculator;
import com.woong.backend.repository.MemberDao;
import com.woong.backend.repository.MemberDaoEx;
import com.woong.backend.service.ChangePasswordService;
import com.woong.backend.service.MemberRegisterService;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.EnableAspectJAutoProxy;

@EnableAspectJAutoProxy
@Configuration
public class AppCtx {

    @Bean
    public ExeTimeAspect exeTimeAspect(){
        return new ExeTimeAspect();
    }

    @Bean
    public Calculator calculator(){
        return new RecCalculator();
    }

    @Bean
    public MemberDao memberDao(){
        return new MemberDao();
    }

    @Bean
    public MemberDaoEx memberDaoEx(){
        return new MemberDaoEx();
    }

    @Bean
    public MemberRegisterService memberRegSvc(){
        return new MemberRegisterService(memberDao());
    }

    @Bean
    public ChangePasswordService changePwdSvc(){
        ChangePasswordService pwdSvc = new ChangePasswordService();
        pwdSvc.setMemberDao(memberDao());
        return pwdSvc;
    }

}
