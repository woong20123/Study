#include <iostream>
#include <thread>
#include <chrono>
#include <windows.h>
using namespace std::literals;

void foo()
{
    auto tid = std::this_thread::get_id();     
    auto handle = GetCurrentThread();
    std::this_thread::sleep_for(1s);
    std::cout  << GetThreadPriority(handle) << std::endl;
}

int main(){
    std::thread t(&foo);
    std::thread::native_handle_type h = t.native_handle();

    std::cout << "ID   : " << t.get_id() << std::endl;
    std::cout << "handle : " << h << std::endl;

    std::this_thread::sleep_for(100ms);

    SetThreadPriority((HANDLE)h, THREAD_PRIORITY_TIME_CRITICAL);

    t.join();
}