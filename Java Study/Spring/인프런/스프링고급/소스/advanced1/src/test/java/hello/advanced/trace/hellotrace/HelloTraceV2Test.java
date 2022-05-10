package hello.advanced.trace.hellotrace;

import hello.advanced.trace.TraceStatus;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class HelloTraceV2Test {

    @Test
    void begin_end(){
        HelloTraceV2 trace = new HelloTraceV2();
        var status1 = trace.begin("hello");
        var status2 = trace.beginSync(status1.getTraceId(), "hello2");

        trace.end(status2);
        trace.end(status1);
    }

    @Test
    void begin_exception() {
        HelloTraceV2 trace = new HelloTraceV2();

        var status1 = trace.begin("ex");
        var status2 = trace.beginSync(status1.getTraceId(), "ex2");

        trace.exceptions(status2, new IllegalArgumentException("ex"));
        trace.exceptions(status1, new IllegalArgumentException("ex"));
    }
}