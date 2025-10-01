#include <stdlib.h>
#include <string.h>
#include <stdint.h>

size_t my_strnlen(const char* src, size_t n) {
    size_t len = 0;
    while (len < n && src[len])
        len++;
    return len;
}

char* strndup(const char* s, size_t n) {
    size_t len = my_strnlen(s, n);
    char* p = malloc(len + 1);
    if (p) {
        memcpy(p, s, len);
        p[len] = '\0';
    }
    return p;
}


char* dirname(char* path)
{
    char* slash = strrchr(path, '/');
    if (!slash)
        return NULL;

    /* Length includes '\0' */
    ptrdiff_t length = slash - path;
    char* dir = malloc(length);

    memcpy(dir, path, length);
    dir[length] = '\0';

    return dir;
}