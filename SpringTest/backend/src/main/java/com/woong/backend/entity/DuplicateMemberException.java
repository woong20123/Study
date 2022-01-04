package com.woong.backend.entity;

public class DuplicateMemberException extends RuntimeException {
    public DuplicateMemberException(String message){
        super(message);
    }
}
