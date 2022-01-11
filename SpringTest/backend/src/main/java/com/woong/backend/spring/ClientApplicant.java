package com.woong.backend.spring;

import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.stereotype.Component;

public class ClientApplicant implements Applicant {
    @Override
    public String show() { return "ClientApplicant";}
}
