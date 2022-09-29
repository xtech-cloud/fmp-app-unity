var FMP = {
    QueryVendor: function()
    {
        const query = window.location.search;
        const vendor = new URLSearchParams(query).get('vendor');
        var buffer = _malloc(lengthBytesUTF8(vendor)+1);
        writeStringToMemory(vendor, buffer);
        return buffer;
    }
}

mergeInto(LibraryManager.library, FMP)
