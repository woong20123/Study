package com.woong.backend.service;

import com.woong.backend.entity.Member;
import com.woong.backend.repository.MemberDao;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;

public class ChangePasswordService {

    @Autowired
    @Qualifier("memberDaoEx")
    private MemberDao memberDao;

    public void ChangePasswordService(String email, String oldPwd, String newPwd){
        Member member = memberDao.selectByEmail(email);

        if(null != member) {
            member.changePassword(oldPwd, newPwd);
            memberDao.update(member);
        }


        System.out.println(memberDao.toString());

        nameplate n = new nameplate("");
        nameplate n2 = new nameplate("");
        n.equals("");
        n.equals(n2);
    }

    public void setMemberDao(MemberDao memberDao){
        this.memberDao = memberDao;
    }

    static public final class nameplate {
        private final String name;

        public nameplate(String name) {
            this.name = name;
        }

        @Override
        public boolean equals(Object o){

            if (o instanceof nameplate)
                return name.equalsIgnoreCase(((nameplate) o).name);

            if( o instanceof String)
                return name.equalsIgnoreCase((String)o);
            return false;
        }
    }
}
