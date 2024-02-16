# VK Auto check and liker.

## Enviroments
Create `.env` file or set enviroments in Docker
- TOKEN - VK access token. If many, use separator - `;`
- GROUPID - VK Group ID. If many, use separator - `;`
- LIKED_LAST_POSTS - Whether to like the group's latest posts; true or false; default - true.
- COUNT_LAST_POSTS - Number of recent posts that need to be liked; int; default - 10.
- COOLDOWN_LIKE - Delay in seconds between the next bark; int; default - 1.
- COOLDOWN_CHECK - Frequency of checking new posts in the group in seconds (Default 60 sec = 1 minute); int

## Where get VK Access token?
* With __access token__, go to [link](https://vkhost.github.io/)  
  Copy everything in quotation marks after __access_token__ and past to enviroment.

## Where me get group ID?
Open any group and open any post  
https://vk.com/{GroupName}?w=wall`-1XXXXXXXX`_`XXX`, where
* -1XXXXXXXX - Group ID
* XXX - Post ID

## Road Map
- [x] Docker support
- [x] Many groups
- [x] Many accounts


## Authors
- [Nikita Shidlovskii](https://github.com/1thenikitz)
- Thanks for first idea - [original repo](https://github.com/AmadeusCode/VKGroupLikes)



