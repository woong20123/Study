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

## innodb Buffer Pool 옵션
### Document 주소 : https://dev.mysql.com/doc/refman/8.0/en/innodb-buffer-pool-resize.html
|옵션명|설 명|
|:--:|:--:|
|innodb_buffer_pool_size||
