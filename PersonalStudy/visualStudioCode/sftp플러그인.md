## Extension
* `SFTP` 검색 후 설치
* .vscode 폴더에 `sftp.json` 추가작업
* 예제 코드
```json 
{
    "name": "backend",
    "host": "111.111.111.225",
    "protocol": "sftp",
    "port": 22,
    "username": "admin",
    "password": "admin!2345",
    "remotePath": "/var/www/",
    "uploadOnSave": true
}

```