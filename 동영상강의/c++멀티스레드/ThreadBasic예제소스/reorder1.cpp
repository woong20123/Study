#include <atomic>
int a = 0;
int b = 0;

// thread A
void foo()
{
    a = b + 1;
    std::atomic_thread_fence(std::memory_order_release);
    b = 1;
}

// thread B
void goo()
{
    if( b == 1){
        // a == 1을 보장 할 수 있을까?
    }
}