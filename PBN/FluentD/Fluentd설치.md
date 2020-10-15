## 설치 방법
### 참고 url : https://docs.fluentd.org/installation/install-by-deb
```bash
# td-agent 4
curl -L https://toolbelt.treasuredata.com/sh/install-ubuntu-bionic-td-agent4.sh | sh

# td-agent 서비스 시작
sudo systemctl start td-agent.service
# td-agent 서비스 상태 확인
sudo systemctl status td-agent.service
# td-agent 재시작
sudo systemctl restart td-agent.service
```
### 설정 파일
#### 경로 : /etc/td-agent/td-agent.conf

```bash
# http 형태로 8888번 포트로 데이터를 전달 받는다.
  <source>
    @type http
    @id input_http
    port 8888
  </source>

#  http://localhost:8888/td.*.* 형태의 경우 out파일로 출력
  <match td.*.*>
    @type tdlog
    @id output_td
    apikey xxxxxx
    auto_create_table
    <buffer>
      @type "file"
      path "/var/log/td-agent/buffer/td"
    </buffer>
    <secondary>
      @type "file"
      path "/var/log/td-agent/failed_records"
      <buffer time>
        path /var/log/td-agent/failed_records
      </buffer>
    </secondary>
  </match>


#  http://localhost:8888/debug.* 형태의 경우 out파일로 출력
  <match debug.**>
    @type stdout
    @id output_stdout
  </match>

```

### 쉘 스크립트로 fluentD에 데이터 전송 
```bash
#!/bin/bash
for i in $(seq 1 10000000); do
        # curl을 통해서 메시지 전달
        curl -X POST -d 'json={"json":"message"}' http://localhost:8888/td.test.11
        sleep .05
        per=i%100
        if [ `expr $i % 100` -eq 0 ]; then
                echo "$i"
                printf "post send\n"
        fi
done
```

## kafka로 데이터 전송
* https://docs.fluentd.org/output/kafka

## kafka 연동 플러그인 
* [fluent-plugin-kafka](https://github.com/fluent/fluent-plugin-kafka)
```bash
sudo apt-get update
sudo apt-get upgrade
sudo apt install gcc
sudo apt install make

# fluent-plugin-kafka의 out plugin이 
sudo gem install ruby-kafka

sudo gem install fluent-plugin-kafka
```
### kafka 연동 플러그인 설명_ Output Plugin
* 이 카프카2 플러그인은 fluentd v1 이상에서 동작함
* 이 플러그인은 쓰여진 데이터를 위한 producer로 ruby-kafka를 사용합니다. 
* 만약에서 ruby-kafka가 kafka 환경에 맞지 않으면 대신 rdkafka2 플러그인을 확인 하십시오
* 이것은 향후 out_kafka 플러그인이 될 것입니다. 
```ini
<match app.**>
  @type kafka2

  brokers               <broker1_host>:<broker1_port>,<broker2_host>:<broker2_port>,.. # Set brokers directly
  topic_key             (string) :default => 'topic'
  partition_key         (string) :default => 'partition'
  partition_key_key     (string) :default => 'partition_key'
  message_key_key       (string) :default => 'message_key'
  default_topic         (string) :default => nil
  default_partition_key (string) :default => nil
  default_message_key   (string) :default => nil
  exclude_topic_key     (bool)   :default => false
  exclude_partition_key (bool)   :default => false
  exclude_partition     (bool)   :default => false
  exclude_message_key   (bool)   :default => false
  get_kafka_client_log  (bool)   :default => false
  headers               (hash)   :default => {}
  headers_from_record   (hash)   :default => {}
  use_default_for_unknown_topic (bool) :default => false
  discard_kafka_delivery_failed (bool) :default => false (No discard)

  <format>
    @type (json|ltsv|msgpack|attr:<record name>|<formatter name>) :default => json
  </format>

  # Optional. See https://docs.fluentd.org/v/1.0/configuration/inject-section
  <inject>
    tag_key tag
    time_key time
  </inject>

  # See fluentd document for buffer related parameters: https://docs.fluentd.org/v/1.0/configuration/buffer-section
  # Buffer chunk key should be same with topic_key. If value is not found in the record, default_topic is used.
  <buffer topic>
    flush_interval 10s
  </buffer>

  # ruby-kafka producer options
  idempotent        (bool)    :default => false
  sasl_over_ssl     (bool)    :default => true
  max_send_retries  (integer) :default => 1
  required_acks     (integer) :default => -1
  ack_timeout       (integer) :default => nil (Use default of ruby-kafka)
  compression_codec (string)  :default => nil (No compression. Depends on ruby-kafka: https://github.com/zendesk/ruby-kafka#compression)
</match>
```

```

## File input
## read apache logs continuously and tags td.apache.access
<source>
  @type tail
  @id input_tail
  <parse>
    @type ltsv
  </parse>
  path /var/log/td-agent/performenceData.log
  pos_file /var/log/td-agent/performenceData.log.pos
  tag kafka.to
</source>


<match **>
  @type kafka2

  brokers 172.19.138.12:9092
  default_topic test-none

  <format>
    @type ltsv
  </format>

  # Optional. See https://docs.fluentd.org/v/1.0/configuration/inject-section

  # See fluentd document for buffer related parameters: https://docs.fluentd.org/v/1.0/configuration/buffer-section
  # Buffer chunk key should be same with topic_key. If value is not found in the record, default_topic is used.
  <buffer test_none>
    @type file
    path /var/log/td-agent/buffer_test
    chunk_limit_size 5MB
    total_limit_size 2048MB
    flush_interval 1s
  </buffer>

  max_send_limit_bytes 100000

  # ruby-kafka producer options
  # compression_codec gzip
</match>
```
* Kafka - Broker: Message size too large 에러 메시지 발생시
* 카프카 /usr/local/kafka/config/server.properties
  * message.max.bytes=15242880 설정

### 백업 폴더
* /tmp/fluent/backup
  
### snappy 압축 알고리즘을 사용하려면
```bash
sudo apt-get install build-essential autoconf automake libtool libsnappy-dev
gem install snappy --no-document
```

### 파일로 전송하려면


## td-agent는 무엇입니까?
* fluent는 성능적인 부분은 C로 구현되어 있고 나머지는 유연성을 위해서 Ruby로 작성되어 있습니다. 
* 일부 사용자는 Ruby 데몬을 설치하고 운영하는데 어려움을 겪을 수 있습니다. 
* Treasure Data Inc가 td-agent라는 Fluentd의 안정적인 배포를 제공하는 이유 입니다.
* 이 설치 가이드는 td-agent v3/v4 용입니다. 
* td-agent v3/v4 코어에서 fluentd v1을 사용합니다. \


## 테스트
gzip : 448MB, 6분
none : 2.1GB, 3분
