#include <iostream>
#include <mutex>

template <class Mutex> class lock_guard
{
public:
    using mutex_type = Mutex;

    explicit lock_guard(Mutex& mtx) : mtx(mtx) { mtx.lock(); }
    ~lock_guard() noexcept { mtx.unlock();}

    lock_guard(const lock_guard &) = delete;
    lock_guard & operator=(const lock_guard&) = delete;
private :
    Mutex & mtx;
};

std::mutex m;

int main()
{
    lock_guard g2(m);
}