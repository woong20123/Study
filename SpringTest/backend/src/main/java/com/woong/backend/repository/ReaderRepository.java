package com.woong.backend.repository;

import com.woong.backend.entity.UserInfo;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface ReaderRepository extends JpaRepository<UserInfo, String> {
    Optional<UserInfo> findByUsername(String username);
}
