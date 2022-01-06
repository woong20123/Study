package com.woong.backend.repository;

public class GlobalApplicant extends Applicant {

    public GlobalApplicant(String name, long grade, String group, String country) {
        super(name, grade, group);
        this.country = country;
    }

    private final String country;
}
