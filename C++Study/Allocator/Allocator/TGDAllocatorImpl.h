#include <memory>

namespace TGD {
	class TGDAllocatorImpl {
	public:
		using allocateKey = unsigned long long;

		TGDAllocatorImpl(size_t objectSize);
		virtual ~TGDAllocatorImpl();

		bool Allocate(void*& allocate);
		void DeAllocate(void* ptr);

	protected:
		bool CheckNewChuck();
		bool newChunk();
	
	private:
		struct Impl;
		std::unique_ptr<Impl> pimpl;
	};
}