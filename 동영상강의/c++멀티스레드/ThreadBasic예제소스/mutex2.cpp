#include <chrono>
#include <iostream>
#include <thread>
#include <string_view>
#include <mutex>
using namespace std::literals;


std::mutex m;
int share_data = 0;

void foo()
{
    if(m.try_lock()){
        share_data = 100;
        std::cout << "using share_data" << std::endl;
        m.unlock();
    }
    else
    {
        std::cout << "뮤텍스 획득 실패" << std::endl;
    }
}

int main()
{
    std::thread t1(foo);
    std::this_thread::sleep_for(20ms);
    std::thread t2(foo);

    t1.join();
    t2.join();
}