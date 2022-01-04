package com.woong.backend.assembler;

import com.woong.backend.repository.MemberDao;
import com.woong.backend.service.ChangePasswordService;
import com.woong.backend.service.MemberRegisterService;

public class Assembler {
    private MemberDao memberDao;
    private MemberRegisterService regSvc;
    private ChangePasswordService pwdSvc;

    public Assembler() {
        memberDao = new MemberDao();
        regSvc = new MemberRegisterService(memberDao);
        pwdSvc = new ChangePasswordService();
        pwdSvc.setMemberDao(memberDao);
    }

    public MemberDao getMemberDao(){
        return memberDao;
    }

    public MemberRegisterService getMemberRegisterService(){
        return regSvc;
    }

    public ChangePasswordService getChangePasswordService() {
        return pwdSvc;
    }
}
