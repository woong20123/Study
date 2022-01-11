package com.woong.backend.spring;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;

public class InterviewRoom {
    @Autowired
    @Qualifier("server")
    private Applicant applicant;

    public void setApplicant(Applicant applicant){
        this.applicant = applicant;
    }

    public void show(){
        System.out.println(applicant.show());
    }
}
