#include <iostream>
#include <fstream>
#include <string>
#include <algorithm>
#include <stdio.h>
#include <regex>
#include <vector>

int how_much;

std::vector<std::string> kerfus;

void checkString(std::string x, std::regex e) {
    std::smatch m;

    while (regex_search(x, m, e))
    {
        kerfus.push_back(m.str());
        x = m.suffix();
    }
}


int main()
{
    int page_counter = 0;
    std::regex e("stream=\"(.*?)\"");
    std::string url;

    while (page_counter < 150)
    {
        url = "\"https://onlineradiobox.com/de/?cs=pl.radiormf&p=" + std::to_string(page_counter) + "\"";

        url = "curl -o data.txt " + url;

        system(url.c_str());

        //system("cls");

        std::string res;
        std::ifstream file("data.txt");
        while (getline(file, res)) {
            checkString(res, e);
        }
        file.close();

        remove("data.txt");
        std::cout << "\nDone page " << page_counter << std::endl;
        page_counter++;
    }
    for (int i = 0; i < kerfus.size(); i++) {
        std::cout << kerfus[i] << std::endl;
        //url = "curl " + kerfus[i] + " -o kerfuś" + std::to_string(i) + ".jpg";
        //system(url.c_str());
        // Write to the file and no overwrite
        std::ofstream outfile;
        outfile.open("radio-niemieckie.txt", std::ios_base::app);
        outfile << kerfus[i] << std::endl;
        outfile.close();
    }
}

