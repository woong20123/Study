socketA, pipeB;
watchedList.add(socketA, FOR_READ);
watchedList.add(pipeB, FOR_READ);
while(events = demultiplexer.watch(watchedList)){
    // 이벤트 루프
    foreach(let event in events){
        data = event.resource.read();
        if(data  == RESOURCE_CLOSED){
            demultiplexer.unwatch(event.resource);
        }else{
            // 실제 데이터를 전달 받아서 처리합니다. 
            consumeData(data);
        }
    }
}