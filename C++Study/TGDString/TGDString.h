#pragma once
#include <string>

namespace TGD
{
    class TGDString
    {
    public:
        TGDString();
        virtual ~TGDString();

        void append();
        size_t length();
        const char * c_str();

        constexpr size_t MaxSize() noexcept
        {
            return static_cast<unsigned int>(-1);
        }

    private :

        // 버퍼 재할당
        void ReallocateGrowby(size_t increaseSize);
        size_t GetGrowth(size_t newSize);

        static constexpr size_t DEFAULT_SIZE = 15;


        char* ptr;
        size_t size;
        size_t reserved;
    };
}

