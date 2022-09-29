var FMP = {
    GetParameters: function()
    {
        var returnStr = window.location.search;
        var buffer = _malloc(lengthBytesUTF8(returnStr)+1);
        writeStringToMemory(returnStr, buffer);
        return buffer;
    }
}

mergeInto(LibraryManager.library, FMP)
