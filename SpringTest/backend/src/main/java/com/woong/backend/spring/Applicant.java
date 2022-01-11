package com.woong.backend.spring;

public interface Applicant {
    default String show(){
        return "applicant";
    }
}
