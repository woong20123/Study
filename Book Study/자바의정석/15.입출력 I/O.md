## 입출력이란
* 컴퓨터 내부나 외부의 장치와 프로그램간의 데이터를 주고 받는 것을 말합니다. 

## 스트림(stream)
* 자바에서 입출력을 수행하려면 즉 한쪽에서 다른 쪽으로 데이터를 전달하려면 두 대상을 연결하고 데이터를 전송한 것을 Stream이라고 합니다.
  * 스트림이란 데이터를 운반하는데 사용되는 연결 통로입니다.
  * 하나의 스트림은 단방향 통신만 가능합니다. 

## 바이트 기반 스트림 - InputStream, OutputStream
* 스트림은 바이트 단위로 데이터를 전송하며 입출력 대상에 따라서 다음과 같은 스트림이 있습니다.
* FileInputStream, FileOutputStream : 파일
* ByteArrayInputStream, ByteArrayOutputStream : 메모리(byte배열)
* PipedInputStream, PipedOutputStream : 프로세스(프로세스간의 통신)
* AudioInputStream, AudioOutputStream : 오디오 장치
* 이들은 모두 InputStream, OutputStream의 자손이며 추상메서드를 구현합니다. 
