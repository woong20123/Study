#include <iostream>
#include <thread>
#include <mutex>
#include <atomic>

std::mutex m;
std::atomic<long> x(0);

void foo()
{
    for(int i = 0; i < 100000; ++i){      
        ++x;
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