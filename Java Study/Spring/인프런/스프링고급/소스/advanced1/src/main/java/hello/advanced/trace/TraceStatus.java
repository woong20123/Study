package hello.advanced.trace;

/**
 * The type Trace status.
 */
public class TraceStatus {

    private TraceId traceId;
    private Long startTimeMs;
    private String Message;

    /**
     * Instantiates a new Trace status.
     *
     * @param traceId     the trace id
     * @param startTimeMs the start time ms
     * @param message     the message
     */
    public TraceStatus(TraceId traceId, Long startTimeMs, String message) {
        this.traceId = traceId;
        this.startTimeMs = startTimeMs;
        Message = message;
    }

    /**
     * Gets trace id.
     *
     * @return the trace id
     */
    public TraceId getTraceId() {
        return traceId;
    }

    /**
     * Gets start time ms.
     *
     * @return the start time ms
     */
    public Long getStartTimeMs() {
        return startTimeMs;
    }

    /**
     * Gets message.
     *
     * @return the message
     */
    public String getMessage() {
        return Message;
    }
}
