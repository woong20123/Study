#include <shared_mutex>
#include <vector>
#include <unordered_map>
#include "TGDAllocatorImpl.h"

namespace TGD {
	template<typename T>
	class TGDAllocate {
	public:		
		using AllocateContainer = std::unordered_map<std::thread::id, std::shared_ptr<TGDAllocatorImpl>>;
		TGDAllocate();
		virtual ~TGDAllocate() {};

		T* Allocate();
		void DeAllocate(T*& ptr);

	protected:
		std::shared_ptr<TGDAllocatorImpl> GetAllocateImpl();

	private:
		std::unordered_map<std::thread::id, std::shared_ptr<TGDAllocatorImpl>> AllocateList;
		std::shared_ptr<TGDAllocatorImpl> allocateImpl;
		std::shared_mutex sm;
	};

	template<typename T>
	TGDAllocate<T>::TGDAllocate() 
	{
		allocateImpl = std::make_shared<TGDAllocatorImpl>(sizeof(T));
	}

	template<typename T>
	T* TGDAllocate<T>::Allocate() {
		void* alloc = nullptr;
		{
			if (!alloc)
			{
				GetAllocateImpl()->Allocate(alloc);
			}
		}
#pragma push_macro("new")
#undef new
		T* newObj = new (alloc) T;
#pragma pop_macro("new")
		return newObj;
	}

	template<typename T>
	void TGDAllocate<T>::DeAllocate(T*& ptr) {
		{
			ptr->~T();
			GetAllocateImpl()->DeAllocate(ptr);
		}
	}

	template<typename T>
	std::shared_ptr<TGDAllocatorImpl> TGDAllocate<T>::GetAllocateImpl()
	{
		const auto threadId = std::this_thread::get_id();
		auto itr = AllocateList.find(threadId);
		if (itr == AllocateList.end())
		{
			auto newallocateImpl = std::make_shared<TGDAllocatorImpl>(sizeof(T));
			auto ret = AllocateList.insert(AllocateContainer::value_type(threadId, newallocateImpl));
			return newallocateImpl;
		}
		return itr->second;
	}
}