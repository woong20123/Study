#include "pch.h"
#include <TGDString.h>
#include <zstd.h>
#include "common.h"

static void compress_orDie(const char* fname, const char* oname)
{
    size_t fSize;
    void* const fBuff = mallocAndLoadFile_orDie(fname, &fSize);
    size_t const cBuffSize = ZSTD_compressBound(fSize);
    void* const cBuff = malloc_orDie(cBuffSize);

    /* Compress.
     * If you are doing many compressions, you may want to reuse the context.
     * See the multiple_simple_compression.c example.
     */
    size_t const cSize = ZSTD_compress(cBuff, cBuffSize, fBuff, fSize, 1);
    CHECK_ZSTD(cSize);

    saveFile_orDie(oname, cBuff, cSize);

    /* success */
    printf("%25s : %6u -> %7u - %s \n", fname, (unsigned)fSize, (unsigned)cSize, oname);

    free(fBuff);
    free(cBuff);
}

static char* createOutFilename_orDie(const char* filename)
{
    size_t const inL = strlen(filename);
    size_t const outL = inL + 5;
    void* const outSpace = malloc_orDie(outL);
    memset(outSpace, 0, outL);
    strcat_s((char*)outSpace, outL, filename);
    strcat_s((char*)outSpace, outL, ".zst");
    return (char*)outSpace;
}
int main(int argc, char* argv[])
{
	TGD::TGDString s;

	std::cout << "[Load Dll Library]" << std::endl;
	s.Append("1111");
	s.Append("2222");
	s.Append("3333");
	s.Append("4444");
	s.Append("5555");
	s.Append("6666");
	s.Append("7777");
	std::cout << s.Cstr() << std::endl;
	getchar();

    const char* const inFilename = argv[1];

    char* const outFilename = createOutFilename_orDie(inFilename);
    compress_orDie(inFilename, outFilename);
    free(outFilename);
    return 0;
}

