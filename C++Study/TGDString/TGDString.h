#pragma once
#include <string>

namespace TGD
{
    class TGDString
    {
    public:
        TGDString();
        virtual ~TGDString();

        TGDString(const TGDString& string);
        TGDString(TGDString&& string) noexcept;


        void Append(const char* chars);
        void Append(const TGDString& string);
        const size_t Length() const noexcept;
        const char * Cstr() const noexcept;

        // String's max size limit 4GB
        constexpr size_t MaxSize() noexcept
        {
            return static_cast<unsigned int>(-1);
        }

    private :
        // 버퍼 재할당
        void ReallocateGrowby(size_t increaseSize);
        size_t GetGrowth(size_t newSize);
        void DefaultAllocate();

        void Append(const char* chars, const size_t appendSize);
        void Allocate(const size_t allocSize, const size_t stringSize);

        static constexpr size_t DEFAULT_SIZE = 15;


        char* ptr;
        size_t size;
        size_t reserved;

        std::string s;
    };
}

