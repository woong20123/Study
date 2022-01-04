package com.woong.backend.service;

import com.woong.backend.dto.RegisterRequest;
import com.woong.backend.entity.Member;
import com.woong.backend.repository.MemberDao;
import javassist.bytecode.DuplicateMemberException;

import java.time.LocalDateTime;

public class MemberRegisterService {
    private MemberDao memberDao;

    public MemberRegisterService(MemberDao memberDao){
        this.memberDao = memberDao;
    }

    public Long regist(RegisterRequest req) throws DuplicateMemberException {
        Member member = memberDao.selectByEmail(req.getEmail());
        if(member != null){
            throw new DuplicateMemberException("dup email" + req.getEmail());
        }

        Member newMember = new Member(0L,
                req.getEmail(), req.getPassword(), req.getName()
                , LocalDateTime.now());
        memberDao.insert(newMember);
        return newMember.getId();

    }
}
