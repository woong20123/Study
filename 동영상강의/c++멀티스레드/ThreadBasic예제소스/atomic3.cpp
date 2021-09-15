#include <iostream>
#include <thread>
#include <atomic>

std::atomic<int > x{0};

void foo()
{
    for(int i = 0; i < 100000; ++i){      
        //++x;
        x.fetch_sub(1, std::memory_order_relaxed);
    }
}

int main()
{
    std::thread t1(foo);
    std::thread t2(foo);
    std::thread t3(foo);

    t1.join();
    t2.join();
    t3.join();
    std::cout << x << std::endl;
}