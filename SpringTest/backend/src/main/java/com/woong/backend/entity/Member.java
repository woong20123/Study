package com.woong.backend.entity;

import lombok.AllArgsConstructor;
import lombok.Data;

import java.time.LocalDateTime;

@AllArgsConstructor
@Data
public class Member {

    private Long id;
    private String email;
    private String password;
    private String name;
    private LocalDateTime registerDateTime;

    public void changePassword(String oldPassword, String newPassword){
        if(!password.equals(oldPassword))
            throw new WrongIDPasswordException();
        this.password = newPassword;
    }
}
