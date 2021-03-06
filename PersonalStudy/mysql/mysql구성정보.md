## mysql 네이밍 규칙
https://launchbylunch.com/posts/2014/Feb/16/sql-naming-conventions/

## mysql insert 속도 저하
* mysql사용중 insert 130만건 이상 작업의 작업 수행시 느려지는 현상 발생
### 저하 데이터 정보
|insert 갯수|카산드라 select|mysql insert|
|:--:|:--:|:--:|
|129534개|4.45 secs|2.1 sec|
|226033개|7.5 secs|4.01 sec|
|419173개|13.75 secs|6.89 sec|
|838581개|26.98 secs|13.23 sec|
|1257917개|40.35 secs|22.83 sec|
|1484194개|48.35.secs|60.sec|
|1677395개|55.24 secs|130.76 secs|

* 아마도 버퍼 풀 할당 부족으로 스왑이 발생한 것으로 예상됨
  * SET GLOBAL innodb_buffer_pool_size 값을 증가시켜보자

### mysql innodb 구성 정보
* https://dev.mysql.com/doc/refman/8.0/en/innodb-init-startup-configuration.html

## mysql 옵션
## Document 주소 : https://dev.mysql.com/doc/refman/8.0/en/
innodb-buffer-pool-resize.html
## 번역
### InnoDB Buffer Pool Size 설정하기
* InnoDB 버퍼 풀 사이즈를 오프라인 또는 서버가 구동중일 때 설정할 수 있습니다. 
* 이 섹션에 설명된 동작은 두 방법에 모두 적용 됩니다. 
* 온라인으로 버퍼 풀 사이즈를 설정하는 방법에 대한 자세한 내용은 아래 링크를 참조하세요
  * https://dev.mysql.com/doc/refman/8.0/en/innodb-buffer-pool-resize.html#innodb-buffer-pool-online-resize
* innodb_buffer_pool_size를 늘리거나 줄이는 작업은 청크 단위로 수행됩니다. 
* 청크 크기는 innodb_buffer_pool_chunk_size 설정옵션으로 정의되며 기본값은 128MB입니다. 
* 버퍼 풀의 사이즈는 innodb_buffer_pool_chunk_size * innodb_buffer_pool_instances의 배수여야 합니다. 
* 만약에 배수가 아닌 값으로 설정하면 자동으로 버퍼 풀의 크기는 배수의 값으로 자동 조정됩니다. 
* 만약 아래와 같이 설정되어 있다고 가정합니다.
  * innodb_buffer_pool_size : 8G
  * innodb_buffer_pool_instances = 16
  * innodb_buffer_pool_chunk_size = 128M
  * innodb_buffer_pool_chunk_size * innodb_buffer_pool_instances = 2G
  * innodb_buffer_pool_size는 2G의 배수입니다. 
* 만약에 innodb_buffer_pool_size = 9G라면 10G로 자동조정 됩니다.
### InnoDB Buffer Pool Chunk Size 설정하기
* innodb_buffer_pool_chunk_size는 1MB 단위로 조정할 수 있지만 명령어나 Mysql 구성 파일로만 설정할 수 있습니다. 
```bash
// 명령어
shell> mysqld --innodb-buffer-pool-chunk-size=134217728

// 설정파일
[mysqld]
innodb_buffer_pool_chunk_size=134217728
```

### innodb_buffer_pool_size
  * InnoDB 스토리지에서 가장 중요한 옵션 
  * 버퍼 풀은 디스크의 데이터를 메모리에 캐싱함과 동시에 데이터의 변경을 버퍼링 하는 역활
  * 일반적으로 클라이언트에 대한 서버 스레드가 사용할 메모리를 제외하고 모두 설정
    * 클라이언트용 서버 스레드가 사용하는 메모리는 세션 단위의 sort_buffer_size, join, read, read_rnd_buffer_size와 클라이언트 서버 스레드로 예측 가능
  * 일반적으로 서버 인스턴스의 50 ~ 80% 메모리 수준으로 설정   
### innodb_buffer_pool_size 설정크기
|커넥션수|4GB|8GB|16GB|32GB|
|:--:|:--:|:--:|:--:|:--:|
|100개이하|1.5 ~ 2GB|5GB|11GB|25GB|
|100~500개|1.5GB|4GB|10GB|24GB|
|500~1000개|1GB|3GB|9GB|23GB|
|1000~1500개|-|3GB|8GB|22GB|
### my.cnf 파일 설정
```
innodb_buffer_pool_size = 25G
innodb_log_buffer_size = 32M
innodb_log_file_size = 2048M
innodb_log_files_in_group = 2
```               

### innodb_log_file_size
* redo 로그파일은 1개이상의 파일로 구성되며 순환되면서 사용됨 
* 해당 값이 너무 작게되면 버퍼 풀이 정상적으로 활용되지 못함
  
### innodb_flush_log_at_trx_commit
* InnoDB에서 트랙젝션이 커밋될 때마다 로그를 디스크에 플러시 할지 결정하는 옵션
* fsync()와 fdatasync()
* 많은 양의 데이터를 처리하면서 데이터가 조금 손실되도 무방하다면 이 값을 0으로 설정해서 성능을 개선할 수 있음
* 장애 발생시 4 ~ 5 초동안 손실이 발생할 수 있음