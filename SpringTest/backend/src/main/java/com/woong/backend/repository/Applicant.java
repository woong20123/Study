package com.woong.backend.repository;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class Applicant {
    Applicant() {}

    private String name;
    private long grade;
    private String group;
}
