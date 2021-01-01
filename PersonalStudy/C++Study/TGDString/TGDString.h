#pragma once
#include <cstdint>
#include <algorithm>

namespace TGD
{
    // TGDBasicString forward declarations
    template<typename Element>
    class TGDBasicString;

    // char를 사용하는 TGDSting 클래스
    using TGDString = TGDBasicString<char>;
    // wchar_t를 사용하는 TGDSting 클래스
    using TGDWString = TGDBasicString<wchar_t>;

    /// @brief TGDString의 구현부 클래스.
    /// @tparam Element Sting에서 사용할 구성 요소를 설정합니다. ex) char, char_t.
    template<typename Element>
	class TGDBasicString
    {
    public:
        TGDBasicString() : ptr(nullptr), size(0), reserved(0)
        {
            DefaultAllocate();
        }
        virtual ~TGDBasicString()
        {
        }

        TGDBasicString(const Element* chars);
        TGDBasicString(const TGDBasicString<Element>& string);
        TGDBasicString(TGDBasicString<Element>&& string) noexcept;

        /// @brief TGDBasicString에 문자열을 추가합니다. 
        /// @param elems 추가할 문자열 정보
        void Append(const Element* elems);

        /// @brief TGDBasicString에 다른 TGDBasicString 문자열을 추가합니다. 
        /// @param string 추가할 다른 TGDBasicString
        void Append(const TGDBasicString<Element>& string);

        /// @brief TGDSting의 내부 문자열 전용 메모리를 예약하는 함수
        /// @param reserved 예약할 메모리 크기
        void Reserve(const size_t reserved);

        /// @brief TGDSting의 문자열 길이를 리턴하는 함수
        /// @return TGDSting의 문자열 길이
        const size_t Length() const noexcept;

        /// @brief TGDBasicString의 내부 문자열을 char * 형태로 리턴하는 함수
        /// @return TGDBasicString의 내부 문자열 주소 
        const Element* Cstr() const noexcept;

        /// @brief TGDBasicString이 비어 있는지 확인하는 함수
        /// @return String의 문자열이 비어 있다면 true리턴
        const bool Empty() const noexcept;

        /// @brief 예약된 메모리 사이즈를 리턴하는 함수
        /// @return 예약된 메모리 사이즈
        const size_t Capacity() const noexcept;

        /// @brief 입력된 문자열과 String을 비교합니다. 
        /// @param elems String과 비교할 문자열
        /// @return 문자가 일치하면 0, 다를경우 -1, 1 
        const int Compare(const Element* elems) const noexcept;

        /// @brief TGD String의 최대 사이즈를 리턴합니다.
        /// @return TGD String의 최대 사이즈
        constexpr static size_t MaxSize() noexcept
        {
            return static_cast<std::uint64_t>(-1);
        }

        constexpr size_t ElementSize()
        {
            return sizeof(Element);
        }

    private:
        /// @brief 인터페이스 Append함수의 실제 구현 함수.
        /// @param chars 추가할 문자열 정보
        /// @param appendSize 추가할 문자열 길이
        void Append(const Element* elems, const size_t appendSize);

        /// @brief 새로운 메모리를 할당하는 함수
        /// @param allocSize 할당할 메모리 크기
        /// @param stringSize TGD에 저장된 문자열의 크기
        void Allocate(const size_t allocSize, const size_t stringSize);

        /// @brief 메모리 재할당이 필요한지 체크한 후에 필요한 경우 메모리를 재할당합니다. 
        /// @param needMemSize 필요한 메모리의 크기
        void ReallocateGrowby(const size_t needMemSize);

        /// @brief 재할당시 필요한 메모리 사이즈를 구하는 함수
        /// @param newSize 최소 새로 할당 해야할 메모리 사이즈 
        /// @return 실제로 할당할 메모리 사이즈 
        const size_t GetGrowth(const size_t newSize) const;

        /// @brief TGDBasicString가 생성시 기본적으로 메모리를 할당하는데 사용하는 함수
        /// 기본 할당 메모리를 사용하면 재할당 횟수를 줄여서 최적화에 도움이 됩니다. 
        void DefaultAllocate();

        /// @brief 입력된 문자열과 TGDBasicString을 비교합니다. 
        /// @param elems 입력된 Element's 
        /// @param compareSize 비교할 문자 길이
        /// @return 같으면 0, 다르면 -1, 1
        int Compare(const Element* elems, const size_t compareSize) const noexcept;

        /// @brief 입력된 Element's 의 크기를 구하는 함수 
        /// @param elems 입력된 Element's 
        /// @return Element's 크기를 리턴
        size_t Strlen(const Element* elems) const noexcept;

        void Memcpy(Element* dest, Element* source, const size_t copySize);
        void MemZeroSet(Element* dest, const size_t setSize);

        // 기본 메모리 할당 사이트
        static constexpr size_t DEFAULT_ALLOCATE_SIZE = 15;

        /// @brief 문자열 저장 주소
        Element* ptr;
        /// @brief 문자열 길이 정보
        size_t size;
        /// @brief 예약된 메모리 길이 정보 
        size_t reserved;
    };


    template<typename Element>
    TGDBasicString<Element>::TGDBasicString(const Element* chars) : ptr(nullptr), size(0), reserved(0)
    {
        Append(chars);
    }

    template<typename Element>
    TGDBasicString<Element>::TGDBasicString(const TGDBasicString<Element>& string) : ptr(nullptr), size(0), reserved(0)
    {
        Append(string.Cstr());
    }

    template<typename Element>
    TGDBasicString<Element>::TGDBasicString(TGDBasicString<Element>&& string) noexcept
    {
        ptr = string.ptr;
        size = string.size;
        reserved = string.reserved;

        string.ptr = nullptr;
        string.size = 0;
        string.reserved = 0;
    }

    template<typename Element>
    void TGDBasicString<Element>::Append(const Element* elems)
    {
        const size_t strLength = Strlen(elems);
        Append(elems, strLength);
    }

    template<typename Element>
    void TGDBasicString<Element>::Append(const TGDBasicString<Element>& str)
    {
        Append(str.Cstr(), str.Length());
    }

    template<typename Element>
    void TGDBasicString<Element>::Reserve(const size_t newReserved)
    {
        if (this->reserved < newReserved) {
            ReallocateGrowby(newReserved);
        }
    }

    template<typename Element>
    const size_t TGDBasicString<Element>::Length() const noexcept
    {
        return size;
    }

    template<typename Element>
    const Element* TGDBasicString<Element>::Cstr() const noexcept
    {
        return ptr;
    }

    template<typename Element>
    const bool TGDBasicString<Element>::Empty() const noexcept
    {
        return size == 0;
    }

    template<typename Element>
    const size_t TGDBasicString<Element>::Capacity() const noexcept
    {
        return reserved;
    }

    template<typename Element>
    const int TGDBasicString<Element>::Compare(const Element* elems) const noexcept
    {
        const size_t strLength = Strlen(elems);
        // if the string length is different
        if (strLength != size) {
            auto compareResult = Compare(elems, std::min(strLength, size));
            if (0 == compareResult)
                return size < strLength ? -1 : 1;

            return compareResult;
        }
        //String length is same
        return Compare(elems, size);
    }

    template<typename Element>
    void TGDBasicString<Element>::Append(const Element* elems, const size_t appendSize)
    {
        const size_t newSize = size + appendSize;

        ReallocateGrowby(newSize);

        Memcpy(ptr + size, const_cast<Element*>(elems), appendSize);
        size += appendSize;
    }

    template<typename Element>
    void TGDBasicString<Element>::ReallocateGrowby(const size_t needMemSize)
    {
        // if newsize large than reserved memory,
        // you need to allocate memory
        if (needMemSize >= reserved)
        {
            const size_t newReallocate = GetGrowth(needMemSize);
            Allocate(newReallocate, size);
        }
    }

    template<typename Element>
    const size_t TGDBasicString<Element>::GetGrowth(const size_t newSize) const
    {
        const auto newAllocSize = newSize | DEFAULT_ALLOCATE_SIZE;

        // can not exceed the max size
        if (newAllocSize > MaxSize())
        {
            return MaxSize();
        }

        return std::max(newAllocSize, reserved + (reserved / 2));
    }

    template<typename Element>
    void TGDBasicString<Element>::DefaultAllocate()
    {
        Allocate(DEFAULT_ALLOCATE_SIZE, 0);
    }

    template<typename Element>
    void TGDBasicString<Element>::Allocate(const size_t allocSize, const size_t stringSize)
    {
        // Allocated new memory
        auto newPtr = new Element[allocSize];
        MemZeroSet(newPtr, allocSize);

        // if exist old memory, initialized it
        if (ptr)
        {
            Memcpy(newPtr, ptr, size);
            delete[] ptr;
        }

        // Assigned new memory
        ptr = newPtr;
        size = stringSize;
        reserved = allocSize;
    }

    template<typename Element>
    int TGDBasicString<Element>::Compare(const Element* elems, const size_t compareSize) const noexcept
    {
        for (size_t i = 0; i < compareSize; i++) {
            if (ptr[i] != elems[i])
            {
                return ptr[i] < elems[i] ? -1 : 1;
            }
        }
        return 0;
    }

    template<typename Element>
    size_t TGDBasicString<Element>::Strlen(const Element* elems) const noexcept
    {
        size_t count = 0;
        do {
            if (0x00 == elems[count])
                break;
            ++count;
        } while (true);
        return count;
    }

    template<typename Element>
    void TGDBasicString<Element>::Memcpy(Element * dest, Element* source, const size_t copySize)
    {
        ::memcpy(reinterpret_cast<void*>(dest), reinterpret_cast<void*>(source), copySize * ElementSize());
    }
    template<typename Element>
    void TGDBasicString<Element>::MemZeroSet(Element* dest, const size_t setSize)
    {
        ::memset(reinterpret_cast<void*>(dest), 0, setSize * ElementSize());
    }
}

