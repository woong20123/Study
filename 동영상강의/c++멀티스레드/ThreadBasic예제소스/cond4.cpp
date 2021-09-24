#include <iostream>
#include <thread>
#include <mutex>
#include <chrono>
#include <condition_variable>
using namespace std::literals;

std::mutex m;
std::condition_variable cv;
int shared_data = 0;
bool data_ready = false;
bool data_process = false;

void consumer()
{
    std::this_thread::sleep_for(200ms);
    std::unique_lock<std::mutex> ul(m);
    cv.wait(ul, [] { return data_ready;});

    std::cout << "consume : " << shared_data << std::endl;
    std::this_thread::sleep_for(1s);
    data_process = true;
    ul.unlock();
    cv.notify_one();
}

void producer()
{
    std::this_thread::sleep_for(2s);
    std::unique_lock<std::mutex> ul(m);
    shared_data = 100;
    data_ready = true;
    std::cout << "produce : " << shared_data << std::endl;
    cv.notify_one();

    cv.wait(ul, [] {return data_process;});

    std::cout << "producer : data processed" << std::endl;
}

int main()
{
    std::thread t1(producer);
    std::thread t2(consumer);
    t1.join();
    t2.join();
}