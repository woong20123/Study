#include <iostream>
#include <thread>
#include <chrono>
using namespace std::literals;

int main() {
    std::this_thread::sleep_for(std::chrono::seconds(3));
    std::this_thread::sleep_for(3s); // 3ms, 3ns, 3min
    
    auto tp1 = std::chrono::steady_clock::now();
    std::this_thread::sleep_until(tp1 + 2000ms);
}