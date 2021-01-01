#pragma once
#include "pch.h"
#include "TGDString.h"


namespace TGD
{
    TGDString::TGDString(const Element* chars) : ptr(nullptr), size(0), reserved(0)
    {
        Append(chars);
    }

    
    TGDString::TGDString(const TGDString& string) : ptr(nullptr), size(0), reserved(0)
    {
        Append(string.Cstr());
    }

    
    TGDString::TGDString(TGDString&& string) noexcept
    {
        ptr = string.ptr;
        size = string.size;
        reserved = string.reserved;

        string.ptr = nullptr;
        string.size = 0;
        string.reserved = 0;
    }

    
    void TGDString::Append(const Element* elems)
    {
        const size_t strLength = Strlen(elems);
        Append(elems, strLength);
    }

    
    void TGDString::Append(const TGDString& str)
    {
        Append(str.Cstr(), str.Length());
    }

    
    void TGDString::Reserve(const size_t newReserved)
    {
        if (this->reserved < newReserved) {
            ReallocateGrowby(newReserved);
        }
    }

    
    const size_t TGDString::Length() const noexcept
    {
        return size;
    }

    
    const TGDString::Element* TGDString::Cstr() const noexcept
    {
        return ptr;
    }

    
    const bool TGDString::Empty() const noexcept
    {
        return size == 0;
    }

    
    const size_t TGDString::Capacity() const noexcept
    {
        return reserved;
    }

    
    const int TGDString::Compare(const Element* elems) const noexcept
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

    
    void TGDString::Append(const Element* elems, const size_t appendSize)
    {
        const size_t newSize = size + appendSize;

        ReallocateGrowby(newSize);

        Memcpy(ptr + size, const_cast<Element*>(elems), appendSize);
        size += appendSize;
    }

    
    void TGDString::ReallocateGrowby(const size_t needMemSize)
    {
        // if newsize large than reserved memory,
        // you need to allocate memory
        if (needMemSize >= reserved)
        {
            const size_t newReallocate = GetGrowth(needMemSize);
            Allocate(newReallocate, size);
        }
    }

    
    const size_t TGDString::GetGrowth(const size_t newSize) const
    {
        const auto newAllocSize = newSize | DEFAULT_ALLOCATE_SIZE;

        // can not exceed the max size
        if (newAllocSize > MaxSize())
        {
            return MaxSize();
        }

        return std::max(newAllocSize, reserved + (reserved / 2));
    }

    
    void TGDString::DefaultAllocate()
    {
        Allocate(DEFAULT_ALLOCATE_SIZE, 0);
    }

    
    void TGDString::Allocate(const size_t allocSize, const size_t stringSize)
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

    
    int TGDString::Compare(const Element* elems, const size_t compareSize) const noexcept
    {
        for (size_t i = 0; i < compareSize; i++) {
            if (ptr[i] != elems[i])
            {
                return ptr[i] < elems[i] ? -1 : 1;
            }
        }
        return 0;
    }

    
    size_t TGDString::Strlen(const Element* elems) const noexcept
    {
        size_t count = 0;
        do {
            if (0x00 == elems[count])
                break;
            ++count;
        } while (true);
        return count;
    }

    
    void TGDString::Memcpy(Element * dest, Element* source, const size_t copySize)
    {
        ::memcpy(reinterpret_cast<void*>(dest), reinterpret_cast<void*>(source), copySize * ElementSize());
    }
    
    void TGDString::MemZeroSet(Element* dest, const size_t setSize)
    {
        ::memset(reinterpret_cast<void*>(dest), 0, setSize * ElementSize());
    }
}

