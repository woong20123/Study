#include <iostream>
#include <thread>
#include <mutex>
#include <chrono>
using namespace std::literals;

std::mutex m1,m2,m3;
std::timed_mutex tm1, tm2, tm3;

std::mutex m;

int main()
{
    std::unique_lock<std::mutex> u1;
    std::unique_lock<std::mutex> u2(m1);                        // 생성자에서 m1.lock

    std::unique_lock<std::mutex> u3(m2, std::try_to_lock);      // m2.try_lock()

    // lock 소유 여부 확인
    if (u3.owns_lock() )
        std::cout << "acquire lock" << std::endl;
    else
        std::cout << "fail to lock" << std::endl;

    m3.lock();
    std::unique_lock<std::mutex> u4(m3, std::adopt_lock);
    

    std::unique_lock<std::timed_mutex> u5(tm1, std::defer_lock);
    auto ret = u5.try_lock_for(2s);

    std::unique_lock<std::timed_mutex> u6(tm2, 2s);     // tm2.try_lock_for() 사용
    std::unique_lock<std::timed_mutex> u7(tm2, std::chrono::steady_clock::now() + 2s);      // tm3.try_lock_until() 사용
}