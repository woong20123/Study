#include <iostream>
#include <thread>
#include <chrono>
using namespace std::literals;

void foo() {}
void goo() {}

int main()
{
    std::thread t1(&foo);
    std::thread t2(&goo);

    t1.swap(t2);

    //std::thread t3 = t1;
    std::thread t4 = std::move(t1);

    
    t2.join();
    t4.join();
}